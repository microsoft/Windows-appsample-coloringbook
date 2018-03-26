// ---------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// The MIT License (MIT)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using ColoringBook.Common;
using Windows.Storage;

namespace ColoringBook.FileIO
{
    // This class is to enable save to be called multiple times simultaneously.
    // This will record that a new save has been called while a previous call is still saving and there is no file access.
    // After it finishes saving, if a new save has been called (any number of times),
    // it will run the save with the data from the latest call.
    public abstract class ColoringBookFileWriter<T1, T2>
    {
        private static object NextJobLock => new object();

        private static bool AnotherJobToProcess { get; set; }

        private static ref FileWritingJob<T1> NextJob => ref _nextJob;

        private static FileWritingJob<T1> _nextJob;

        public async Task WriteAsync(StorageFolder folder, string fileName, T1 data)
        {
            var secondaryFileName = fileName + Tools.GetResourceString("FileType/secondarySaveFile");

            var file = await Tools.CreateFileAsync(folder, secondaryFileName);

            var newJob = new FileWritingJob<T1>(fileName, secondaryFileName, folder, file, data);
            await NewJobAsync(newJob);
        }

        protected abstract Task<T2> GetStreamAsync(StorageFile file);

        protected abstract Task WriteToFile(T2 stream, T1 data);

        private async Task NewJobAsync(FileWritingJob<T1> job)
        {
            var stream = await GetStreamAsync(job.Outfile);
            if (stream != null)
            {
                await WriteToFile(stream, job.Data);
                await OnWriteCompletedAsync(job);
            }
            else
            {
                lock (NextJobLock)
                {
                    AnotherJobToProcess = true;
                    NextJob = job;
                }
            }
        }

        public async Task RunNextJobAsync()
        {
            FileWritingJob<T1> nextJob;

            lock (NextJobLock)
            {
                if (!AnotherJobToProcess)
                {
                    return;
                }

                nextJob = NextJob;
                AnotherJobToProcess = false;
            }

            await RunJobAsync(nextJob);
        }

        private async Task RunJobAsync(FileWritingJob<T1> job)
        {
            var stream = await GetStreamAsync(job.Outfile);
            if (stream != null)
            {
                await WriteToFile(stream, job.Data);
                await OnWriteCompletedAsync(job);
            }
        }

        private async Task OnWriteCompletedAsync(FileWritingJob<T1> job)
        {
            // Replace actual file with the backup.
            var originalFile = await Tools.GetFileAsync(job.Folder, job.FileName);
            var backupFile = await Tools.GetFileAsync(job.Folder, job.BackupName);

            if (originalFile == null)
            {
                await backupFile.RenameAsync(job.FileName);
            }
            else
            {
                await backupFile.CopyAndReplaceAsync(originalFile);
                await backupFile.DeleteAsync();
            }

            // Run next job.
            await RunNextJobAsync();
        }

        private struct FileWritingJob<T>
        {
            public FileWritingJob(string originalName, string backupName,
                StorageFolder directory, StorageFile outFile, T data)
            {
                FileName = originalName;
                BackupName = backupName;
                Folder = directory;
                Outfile = outFile;
                Data = data;
            }

            public string FileName { get; }

            public string BackupName { get; }

            public StorageFolder Folder { get; }

            public StorageFile Outfile { get; }

            public T Data { get; }
        }
    }
}
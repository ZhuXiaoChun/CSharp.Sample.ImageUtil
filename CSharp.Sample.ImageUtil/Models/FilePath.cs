using BaoXia.Utils;
using BaoXia.Utils.Extensions;

namespace CSharp.Sample.ImageUtil.Models
{
    public class FilePath
    {

        ////////////////////////////////////////////////
        // @静态常量
        ////////////////////////////////////////////////

        #region 静态常量

        public readonly static StringWithFunctionExpression.FunctionExpressionInfo TimeStampFunction = new("#TimeStamp", "(", ")#");

        public readonly static StringWithFunctionExpression.FunctionExpressionInfo UserIdFunction = new("#UserId", "(", ")#");

        public readonly static StringWithFunctionExpression.FunctionExpressionInfo FileIdFunction = new("#FileId", "(", ")#");

        public readonly static StringWithFunctionExpression.FunctionExpressionInfo FileExtensionNameFunction = new("#FileExtensionName", "(", ")#");

        public readonly static StringWithFunctionExpression.FunctionExpressionInfo FileTagsFunction = new("#FileTags", "(", ")#");

        public readonly static StringWithFunctionExpression.FunctionExpressionInfo ImageSizeFunction = new("#ImageSize", "(", ")#");

        public readonly static StringWithFunctionExpression.FunctionExpressionInfo[] FunctionDefines =
        {
                        TimeStampFunction,
                        UserIdFunction,
                        FileIdFunction,
                        FileExtensionNameFunction,
                        FileTagsFunction,
                        ImageSizeFunction
                };

        #endregion


        ////////////////////////////////////////////////
        // @类方法
        ////////////////////////////////////////////////

        #region 类方法

        protected static string? CreateFilePathWithFilePathFormatter(
                string? filePathFormatter,
                int fileId,
                int userId,
                string? fileExtensionName,
                string[]? fileTags,
                Func<StringWithFunctionExpression.FunctionExpressionInfo, string?>? toInvokeFunction)
        {
            if (filePathFormatter == null
                || filePathFormatter.Length < 1)
            {
                return null;
            }

            string? filePath
                    = StringWithFunctionExpression.CreateStringByComputeFunctionExpression(
                    filePathFormatter,
                    FilePath.FunctionDefines,
                    (StringWithFunctionExpression.FunctionExpressionInfo functionExpressionInfo) =>
                    {
                        string? functionResult = null;
                        if (toInvokeFunction != null)
                        {
                            functionResult = toInvokeFunction(functionExpressionInfo);
                            if (functionResult != null)
                            {
                                return functionResult;
                            }
                        }

                        if (FilePath.TimeStampFunction.Name.EqualsIgnoreCase(
                                            functionExpressionInfo.Name))
                        {
                            var timeStampFormatter = functionExpressionInfo.GetFirstParam();
                            if (timeStampFormatter?.Length > 0)
                            {
                                // !!!
                                functionResult = DateTime.Now.ToString(timeStampFormatter);
                                // !!!
                            }
                        }
                        else if (FilePath.UserIdFunction.Name.EqualsIgnoreCase(
                                            functionExpressionInfo.Name))
                        {
                            // !!!
                            functionResult = userId.ToString();
                            // !!!
                        }
                        else if (FilePath.FileIdFunction.Name.EqualsIgnoreCase(
                                            functionExpressionInfo.Name))
                        {
                            // !!!
                            functionResult = fileId.ToString();
                            // !!!
                        }
                        else if (FilePath.FileExtensionNameFunction.Name.EqualsIgnoreCase(
                                            functionExpressionInfo.Name))
                        {
                            // !!!
                            functionResult = fileExtensionName?.ToString();
                            // !!!
                        }
                        else if (FilePath.FileTagsFunction.Name.EqualsIgnoreCase(
                                            functionExpressionInfo.Name))
                        {
                            // !!!
                            if (fileTags?.Length > 0)
                            {
                                functionResult = StringExtension.StringWithStrings(fileTags, "_");
                            }
                            // !!!
                        }
                        else if (FilePath.ImageSizeFunction.Name.EqualsIgnoreCase(
                                            functionExpressionInfo.Name))
                        { }
                        return functionResult;
                    });
            return filePath;
        }

        public static string? CreateAbsolutePathWithFilePathFormatter(
            string? filePathFormatter,
            int fileId,
            int userId,
            string? fileExtensionName,
            string[]? fileTags,
            Func<StringWithFunctionExpression.FunctionExpressionInfo, string?>? toInvokeFunction = null)
        {
            if (string.IsNullOrEmpty(filePathFormatter))
            {
                return null;
            }

            var filePath = FilePath.CreateFilePathWithFilePathFormatter(
                    filePathFormatter,
                    fileId,
                    userId,
                    fileExtensionName,
                    fileTags,
                    toInvokeFunction);
            string? fileAbsoluteFilePath;
            if (System.IO.Path.IsPathRooted(filePath) == true)
            {
                fileAbsoluteFilePath = filePath;
            }
            else
            {
                fileAbsoluteFilePath
                        = BaoXia.Utils.Environment.ApplicationDirectoryPath
                        + filePath;
            }
            return fileAbsoluteFilePath;
        }

        public static string? CreateAbsoluteUriWithFileUriFormatter(
            string? fileUriFormatter,
            int fileId,
            int userId,
            string? fileExtensionName,
            string[]? fileTags,
            Func<StringWithFunctionExpression.FunctionExpressionInfo, string?>? toInvokeFunction = null)
        {
            if (string.IsNullOrEmpty(fileUriFormatter))
            {
                return null;
            }
            var fileUri = FilePath.CreateFilePathWithFilePathFormatter(
                    fileUriFormatter,
                    fileId,
                    userId,
                    fileExtensionName,
                    fileTags,
                    toInvokeFunction);
            string? fileAbsoluteFileUri = fileUri;
            { }
            return fileAbsoluteFileUri;
        }

        #endregion
    }
}

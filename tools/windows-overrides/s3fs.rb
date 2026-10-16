class S3fs < Formula
  desc "Mount an S3 bucket as a folder or drive (via rclone and WinFsp)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "s3fs", "s3fs"
  end

  test do
    assert_match "Amazon Simple Storage Service File System", shell_output("#{bin}/s3fs --version")
    assert_match "passwd-s3fs", shell_output("#{bin}/s3fs --help")
  end
end

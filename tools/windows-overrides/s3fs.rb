class S3fs < Formula
  desc "Mount an S3 bucket as a folder or drive (via rclone and WinFsp)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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

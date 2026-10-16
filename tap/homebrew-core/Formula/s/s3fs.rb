class S3fs < Formula
  desc "Mount an S3 bucket as a folder or drive (via rclone and WinFsp)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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

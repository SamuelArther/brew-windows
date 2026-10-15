class Rclone < Formula
  desc "Rsync for cloud storage"
  homepage "https://rclone.org/"
  version "1.75.2"
  url "https://github.com/rclone/rclone/releases/download/v1.75.2/rclone-v1.75.2-windows-amd64.zip"
  sha256 "8a9db2bad1dc79a65b3348fd857069598e2aab7cf4d118b77b1df23055aff818"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "rclone.exe"
  end

  test do
    (testpath/"file1.txt").write "Test!"
    system bin/"rclone", "copy", testpath/"file1.txt", testpath/"dist"
    assert_match File.read(testpath/"file1.txt"), File.read(testpath/"dist/file1.txt")
  end
end

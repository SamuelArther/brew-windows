class Megacmd < Formula
  desc "Command-line client for mega.co.nz storage service"
  homepage "https://github.com/t3rm1n4l/megacmd"
  url "https://github.com/t3rm1n4l/megacmd/releases/download/0.016/megacmd_0.016_windows_amd64.zip"
  sha256 "78bf4a0b32da45249c3a6c399e721228109f35ad7f9c0ff4c4467ebd7bb15423"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/t3rm1n4l/megacmd/archive/refs/tags/0.016.tar.gz"
  end

  def install
    bin.install "megacmd.exe"
  end

  test do
    system bin/"megacmd", "--version"
  end
end

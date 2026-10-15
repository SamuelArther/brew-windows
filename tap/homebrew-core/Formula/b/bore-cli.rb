class BoreCli < Formula
  desc "Modern, simple TCP tunnel in Rust that exposes local ports to a remote server"
  homepage "https://github.com/ekzhang/bore"
  url "https://github.com/ekzhang/bore/releases/download/v0.6.0/bore-v0.6.0-x86_64-pc-windows-msvc.zip"
  sha256 "01709c64fe2787cdc9a21d7030b0f08ad72dff0c36b7ecb72f4f667a55a34b4f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ekzhang/bore/archive/refs/tags/v0.6.0.tar.gz"
  end

  def install
    bin.install "bore.exe"
  end

  test do
    assert_path_exists bin/"bore.exe"
  end
end

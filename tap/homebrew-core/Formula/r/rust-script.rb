class RustScript < Formula
  desc "Run Rust files and expressions as scripts without any setup or compilation step"
  homepage "https://rust-script.org"
  url "https://github.com/fornwall/rust-script/releases/download/0.36.0/rust-script-x86_64-pc-windows-msvc.zip"
  sha256 "9c724d7708929b53b68003e82a9eb0324870615aa6919bf25b7bf898b2c750c5"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/fornwall/rust-script/archive/refs/tags/0.36.0.tar.gz"
  end

  def install
    bin.install "rust-script.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/rust-script --version 2>&1")
  end
end

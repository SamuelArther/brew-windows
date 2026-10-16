class CargoDist < Formula
  desc "Tool for building final distributable artifacts and uploading them to an archive"
  homepage "https://axodotdev.github.io/cargo-dist/"
  url "https://github.com/axodotdev/cargo-dist/releases/download/v0.33.0/cargo-dist-x86_64-pc-windows-msvc.zip"
  sha256 "9a36d70795e14326a5ec4bf17aee085df00ab85a322739291ea5d4b1b5f693cd"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/axodotdev/cargo-dist/archive/refs/tags/v0.33.0.tar.gz"
  end

  def install
    bin.install "dist.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dist --version 2>&1")
  end
end

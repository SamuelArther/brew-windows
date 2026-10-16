class Arf < Formula
  desc "Modern R console with syntax highlighting and fuzzy search"
  homepage "https://github.com/eitsupi/arf"
  url "https://github.com/eitsupi/arf/releases/download/v0.6.0/arf-console-x86_64-pc-windows-msvc.zip"
  sha256 "c0ace0b1451a81b5b63a268f707298999702bb97f6f5937c1835b93db72e0a20"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/eitsupi/arf/archive/refs/tags/v0.6.0.tar.gz"
  end

  def install
    bin.install "arf.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/arf --version 2>&1")
  end
end

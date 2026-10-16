class Cabin < Formula
  desc "Package manager and build system for C/C++"
  homepage "https://cabinpkg.com"
  url "https://github.com/cabinpkg/cabin/releases/download/0.17.0/cabin-0.17.0-x86_64-pc-windows-msvc.zip"
  sha256 "1a8d2f73668b850250203e7c36fb447171fb20ef04b291690be9f670e6d1ce7b"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/cabinpkg/cabin/archive/refs/tags/0.17.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "cabin.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cabin --version 2>&1")
  end
end

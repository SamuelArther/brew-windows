class Modsurfer < Formula
  desc "Validate, audit and investigate WebAssembly binaries"
  homepage "https://dylibso.com/products/modsurfer/"
  url "https://github.com/dylibso/modsurfer/releases/download/v0.0.10/modsurfer-x86_64-pc-windows-msvc.zip"
  sha256 "cc77fb040ef159d676bb03d302835471da9194d9a71bcb8fd2d33305518a0f71"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/dylibso/modsurfer/archive/refs/tags/v0.0.10.tar.gz"
  end

  def install
    bin.install "modsurfer.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/modsurfer --version 2>&1")
  end
end

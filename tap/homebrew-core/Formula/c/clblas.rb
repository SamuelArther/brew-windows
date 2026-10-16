class Clblas < Formula
  desc "Library containing BLAS functions written in OpenCL"
  homepage "https://github.com/clMathLibraries/clBLAS"
  url "https://github.com/clMathLibraries/clBLAS/releases/download/v2.12/clBLAS-2.12.0-Windows-x64.zip"
  sha256 "864c9f1898471ddb4b7d0bc901146ac103c023b232e3950f9e00bcd44a4dcfbe"
  license "Apache-2.0"
  revision 1

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/clMathLibraries/clBLAS/archive/refs/tags/v2.12.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/clBLAS-tune.exe", "clBLAS-tune"
  end

  test do
    system bin/"clBLAS-tune", "--help"
  end
end

class Clinfo < Formula
  desc "Print information about OpenCL platforms and devices"
  homepage "https://github.com/Oblomov/clinfo"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-clinfo-3.1.26.09.26-1-any.pkg.tar.zst"
  sha256 "29a4ef79aff1d89cb2b69c3989ad4dca2005763a0d1e3174d9ff7e6a60720127"
  license "CC0-1.0"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  resource "mingw-w64-ucrt-x86_64-opencl-icd" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-opencl-icd-2026.05.29-1-any.pkg.tar.zst"
    sha256 "4e0dab8d5cc3da24edb716d78b013c1a36fc111b06f0f286a949218665d71a98"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/clinfo.exe", "clinfo"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/clinfo --version 2>&1")
  end
end

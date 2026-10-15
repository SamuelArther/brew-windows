class Soapyhackrf < Formula
  desc "SoapySDR HackRF module"
  homepage "https://github.com/pothosware/SoapyHackRF/wiki"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-soapyhackrf-0.3.4-3-any.pkg.tar.zst"
  sha256 "90d8f0eaac65ec39e7fe0213cee3b5b9c50b1887cfb52ea4baffa5b28cdacb7d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/pothosware/SoapyHackRF/archive/refs/tags/soapy-hackrf-0.3.5.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"SoapySDR/modules0.8/libHackRFSupport.dll"
  end
end

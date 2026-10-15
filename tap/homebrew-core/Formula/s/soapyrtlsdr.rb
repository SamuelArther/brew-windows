class Soapyrtlsdr < Formula
  desc "SoapySDR RTL-SDR Support Module"
  homepage "https://github.com/pothosware/SoapyRTLSDR/wiki"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-soapyrtlsdr-0.3.3-3-any.pkg.tar.zst"
  sha256 "fb49bab78d6170ec69d06b407672564d204d1931a204826711e841b9c3388763"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/pothosware/SoapyRTLSDR/archive/refs/tags/soapy-rtlsdr-0.3.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"SoapySDR/modules0.8/librtlsdrSupport.dll"
  end
end

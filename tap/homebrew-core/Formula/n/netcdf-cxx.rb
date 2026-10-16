class NetcdfCxx < Formula
  desc "C++ libraries and utilities for NetCDF"
  homepage "https://www.unidata.ucar.edu/software/netcdf/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-netcdf-cxx-4.3.1-7-any.pkg.tar.zst"
  sha256 "09a31890e355638eb9b33f57e21f39701761c9cbffffc2569d07974878fc5a92"
  license "NetCDF"
  revision 3

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/Unidata/netcdf-cxx4/archive/refs/tags/v4.3.1.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ncAtt.h"
    assert_path_exists lib/"libh5bzip2.dll.a"
    assert_path_exists bin/"libnetcdf_c++4-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/netcdf-cxx4.pc").read
  end
end

class NetcdfFortran < Formula
  desc "Fortran libraries and utilities for NetCDF"
  homepage "https://www.unidata.ucar.edu/software/netcdf/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-netcdf-fortran-4.6.3-1-any.pkg.tar.zst"
  sha256 "09d7657690218b5b58c6fa534d50a08eabf538e03e17a8366290c4f1c8fbab40"
  license "NetCDF"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/Unidata/netcdf-fortran/archive/refs/tags/v4.6.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists lib/"libnetcdff.dll.a"
    assert_path_exists bin/"libnetcdff-7.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/netcdf-fortran.pc").read
  end
end

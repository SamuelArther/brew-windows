class LibimobiledeviceGlue < Formula
  desc "Library with common system API code for libimobiledevice projects"
  homepage "https://libimobiledevice.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libimobiledevice-glue-1.3.2-1-any.pkg.tar.zst"
  sha256 "d089c718ab846c5bd1f41fb2788a976fdbda13c9f09128a670eca578da0bc34f"
  license "LGPL-2.1-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/libimobiledevice/libimobiledevice-glue/releases/download/1.3.3/libimobiledevice-glue-1.3.3.tar.bz2"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libimobiledevice-glue/sha.h"
    assert_path_exists lib/"libimobiledevice-glue-1.0.dll.a"
    assert_path_exists bin/"libimobiledevice-glue-1.0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libimobiledevice-glue-1.0.pc").read
  end
end

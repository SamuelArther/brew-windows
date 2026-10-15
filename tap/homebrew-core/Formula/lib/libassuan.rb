class Libassuan < Formula
  desc "Assuan IPC Library"
  homepage "https://www.gnupg.org/related_software/libassuan/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libassuan-2.5.7-2-any.pkg.tar.zst"
  sha256 "fcca8e84b72910a3aa56053e706e2b1efc03df826a26bb6eab1c321991db6826"
  license all_of: [
    "LGPL-2.1-or-later",
    "GPL-3.0-or-later", # assuan.info
    "FSFULLR", # libassuan-config, libassuan.m4
  ]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://gnupg.org/ftp/gcrypt/libassuan/"
    regex(/href=.*?libassuan[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"assuan.h"
    assert_path_exists lib/"libassuan.dll.a"
    assert_path_exists bin/"libassuan-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libassuan.pc").read
  end
end

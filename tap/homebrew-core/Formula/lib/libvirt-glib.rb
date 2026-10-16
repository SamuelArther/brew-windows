class LibvirtGlib < Formula
  desc "Libvirt API for glib-based programs"
  homepage "https://libvirt.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libvirt-glib-5.0.0-6-any.pkg.tar.zst"
  sha256 "69397d63afdd0cd29e2cfb50e10e49965fc805b68228c995ee43bd92cd114824"
  license "LGPL-2.1-or-later"
  revision 1

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.libvirt.org/glib/"
    regex(/href=.*?libvirt-glib[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"libvirt-glib-1.0/libvirt-glib/libvirt-glib.h"
    assert_path_exists lib/"libvirt-gconfig-1.0.dll.a"
    assert_path_exists bin/"libvirt-gconfig-1.0-0.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libvirt-gconfig-1.0.pc").read
  end
end

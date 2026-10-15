class Unicorn < Formula
  desc "Lightweight multi-architecture CPU emulation framework"
  homepage "https://www.unicorn-engine.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-unicorn-2.1.4-5-any.pkg.tar.zst"
  sha256 "161296f12ae2e9b87221f8d94bc34b0044f3e8fd7e85da44d1e2eac8d30dd898"
  license all_of: [
    "GPL-2.0-only",
    "GPL-2.0-or-later", # glib, qemu
  ]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/unicorn-engine/unicorn/archive/refs/tags/2.1.4.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"unicorn/arm.h"
    assert_path_exists lib/"libunicorn.dll.a"
    assert_path_exists bin/"libunicorn.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/unicorn.pc").read
  end
end

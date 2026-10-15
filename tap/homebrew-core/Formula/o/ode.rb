class Ode < Formula
  desc "Simulating articulated rigid body dynamics"
  homepage "https://www.ode.org/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ode-0.16.6-2-any.pkg.tar.zst"
  sha256 "32b48fcdd8fdd5fee227997f9b0ea4f74ac0688df6bfc4e78ef4937d4ff76a4c"
  license any_of: ["LGPL-2.1-or-later", "BSD-3-Clause"]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url :head
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"ode/ode.h"
    assert_path_exists lib/"libode_double.dll.a"
    assert_path_exists bin/"libode_double.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/ode.pc").read
  end
end

class Libvidstab < Formula
  desc "Transcode video stabilization plugin"
  homepage "https://github.com/georgmartius/vid.stab"
  url "https://github.com/georgmartius/vid.stab/archive/refs/tags/v1.1.2.tar.gz"
  sha256 "96db34d48a9e3aa13736a48744b56dfb76731ac9bb5193c716de8534c9fd709d"
  license "GPL-2.0-or-later"
  compatibility_version 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "29bf9af690c6dfa03ef1f9eb3d2d70e63c288203e18c1cac0f56b00885f4abe8"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :test

  deny_network_access!

  def install
    args = %w[
      -DUSE_OMP=OFF
      -DCMAKE_POLICY_VERSION_MINIMUM=3.5
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <vid.stab/libvidstab.h>
      #include <stdio.h>
      int main() {
        printf("libvidstab version: %s\\n", LIBVIDSTAB_VERSION);
        return 0;
      }
    C

    flags = shell_output("pkgconf --cflags --libs vidstab").chomp.split
    system ENV.cc, "test.c", "-o", "test", *flags
    system "./test"
  end
end

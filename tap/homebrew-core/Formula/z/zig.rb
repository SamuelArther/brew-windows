class Zig < Formula
  desc "Programming language designed for robustness, optimality, and clarity"
  homepage "https://ziglang.org/"
  version "0.17.0"
  url "https://ziglang.org/download/0.17.0/zig-x86_64-windows-0.17.0.zip"
  sha256 "b5663f69581dcf391293fbf16c06cb80d81d806545ce618b4d0bab7f0eb8c428"
  license "MIT"

  livecheck do
    url "https://ziglang.org/download/"
    regex(/href=.*?zig[._-]x86_64[._-]windows[._-]v?(\d+(?:\.\d+)+)\.zip/i)
  end

  # Official prebuilt Zig toolchain; zig.exe finds its bundled lib directory in place.

  def install
    install_tree
    windows_exe "libexec/zig.exe", "zig"
  end

  test do
    (testpath/"hello.zig").write <<~ZIG
      const std = @import("std");
      pub fn main() void {
          std.debug.print("Hello, world!", .{});
      }
    ZIG
    assert_match version.to_s, shell_output("#{bin}/zig version")
    system bin/"zig", "build-exe", "hello.zig"
  end
end

class Lv2 < Formula
  desc "Portable plugin standard for audio systems"
  homepage "https://lv2plug.in/"
  url "https://lv2plug.in/spec/lv2-1.18.10.tar.xz"
  sha256 "78c51bcf21b54e58bb6329accbb4dae03b2ed79b520f9a01e734bd9de530953f"
  license any_of: ["0BSD", "ISC"]
  compatibility_version 1
  head "https://gitlab.com/lv2/lv2.git", branch: "main"

  livecheck do
    url "https://lv2plug.in/spec/"
    regex(/href=.*?lv2[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "24531d675f2d1b68e8e95f15230f34ba7db316b3ac9fc2ed04a4fac8bbd6d425"
  end

  depends_on "meson" => :build
  depends_on "ninja" => :build

  def install
    system "meson", "setup", "build", "-Dplugins=disabled", "-Dlv2dir=#{lib}/lv2", *std_meson_args
    system "meson", "compile", "-C", "build", "--verbose"
    system "meson", "install", "-C", "build"

    (pkgshare/"example").install "plugins/eg-amp.lv2/amp.c"
  end

  test do
    # Try building a simple lv2 plugin
    dynamic_flag = OS.mac? ? "-dynamiclib" : "-shared"
    system ENV.cc, pkgshare/"example/amp.c", "-I#{include}",
           "-DEG_AMP_LV2_VERSION=1.0.0", "-DHAVE_LV2=1", "-fPIC", dynamic_flag,
           "-o", shared_library("amp")
  end
end

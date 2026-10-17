class Flif < Formula
  desc "Free Loseless Image Format"
  homepage "https://flif.info/"
  url "https://github.com/FLIF-hub/FLIF/archive/refs/tags/v0.4.tar.gz"
  sha256 "cc98313ef0dbfef65d72bc21f730edf2a97a414f14bd73ad424368ce032fdb7f"
  license "LGPL-3.0-or-later"
  head "https://github.com/FLIF-hub/FLIF.git", branch: "master"

  deprecate! date: "2026-07-17", because: :deprecated_upstream, replacement_formula: "jpeg-xl"
  disable! date: "2027-01-17", because: :deprecated_upstream, replacement_formula: "jpeg-xl"

  bottle do
    sha256 cellar: :any, x86_64_windows: "c6f8cc47f39354980469fe4ffd128ceac136e443cb5c489663dbd551e8acb539"
  end

  depends_on "cmake" => :build
  depends_on "pkgconf" => :build
  depends_on "libpng"
  depends_on "sdl2-compat"

  resource "homebrew-test_c", :test do
    url "https://raw.githubusercontent.com/FLIF-hub/FLIF/dcc2011/tools/test.c"
    sha256 "a20b625ba0efdb09ad21a8c1c9844f686f636656f0e9bd6c24ad441375223afe"
  end

  allow_network_access! :test

  def install
    args = %W[
      -DCMAKE_INSTALL_RPATH=#{rpath}
    ]
    # Workaround to build with CMake 4
    args << "-DCMAKE_POLICY_VERSION_MINIMUM=3.5"
    system "cmake", "-S", "src", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
    doc.install "doc/flif.pdf"
  end

  test do
    testpath.install resource("homebrew-test_c")
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lflif", "-o", "test"
    system "./test", "dummy.flif"
    system bin/"flif", "-i", "dummy.flif"
    system bin/"flif", "-I", test_fixtures("test.png"), "test.flif"
    system bin/"flif", "-d", "test.flif", "test.png"
    assert_path_exists testpath/"test.png", "Failed to decode test.flif"
  end
end

class Libwbxml < Formula
  desc "Library and tools to parse and encode WBXML documents"
  homepage "https://github.com/libwbxml/libwbxml"
  url "https://github.com/libwbxml/libwbxml/archive/refs/tags/libwbxml-0.11.10.tar.gz"
  sha256 "027b77ab7c06458b73cbcf1f06f9cf73b65acdbb2ac170b234c1d736069acae4"
  license "LGPL-2.1-or-later"
  head "https://github.com/libwbxml/libwbxml.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "873a6bc52f005355d14b4b59d0c17e8276b7e285bc4c54783016a4cf77fd6b44"
  end

  depends_on "cmake" => :build
  depends_on "doxygen" => :build
  depends_on "graphviz" => :build
  depends_on "pkgconf" => :build
  depends_on "wget"

  uses_from_macos "expat"

  def install
    args = %W[
      -DBUILD_DOCUMENTATION=ON
      -DCMAKE_INSTALL_RPATH=#{rpath}
    ]
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"input.xml").write <<~XML
      <?xml version="1.0"?>
      <!DOCTYPE sl PUBLIC "-//WAPFORUM//DTD SL 1.0//EN" "http://www.wapforum.org/DTD/sl.dtd">
      <sl href="http://www.xyz.com/ppaid/123/abc.wml"></sl>
    XML

    system bin/"xml2wbxml", "-o", "output.wbxml", "input.xml"
    assert_path_exists testpath/"output.wbxml"
  end
end

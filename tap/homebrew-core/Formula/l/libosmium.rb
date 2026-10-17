class Libosmium < Formula
  desc "Fast and flexible C++ library for working with OpenStreetMap data"
  homepage "https://osmcode.org/libosmium/"
  url "https://github.com/osmcode/libosmium/archive/refs/tags/v2.23.1.tar.gz"
  sha256 "eb47d8163396c73870dbf27819dcadda0c56f0191935c4f8659d1e61b261552f"
  license "BSL-1.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fab48d1368c8295aab55dcf75965a3e858fc4db61057386eb2e44c7120a4a529"
  end

  depends_on "boost" => :build
  depends_on "cmake" => :build
  depends_on "protozero" => :build
  depends_on "lz4"

  uses_from_macos "bzip2"
  uses_from_macos "expat"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def install
    args = %W[
      -DBUILD_EXAMPLES=OFF
      -DBUILD_WITH_CCACHE=OFF
      -DINSTALL_GDALCPP=ON
      -DINSTALL_UTFCPP=ON
      -DPROTOZERO_INCLUDE_DIR=#{formula_opt_include("protozero")}
    ]

    # We only install headers, so we can skip `cmake --build`.
    system "cmake", "-S", ".", "-B", "build", *args, *std_cmake_args
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.osm").write <<~XML
      <?xml version="1.0" encoding="UTF-8"?>
      <osm version="0.6" generator="handwritten">
        <node id="1" lat="0.001" lon="0.001" user="Dummy User" uid="1" version="1" changeset="1" timestamp="2015-11-01T19:00:00Z"></node>
        <node id="2" lat="0.002" lon="0.002" user="Dummy User" uid="1" version="1" changeset="1" timestamp="2015-11-01T19:00:00Z"></node>
        <way id="1" user="Dummy User" uid="1" version="1" changeset="1" timestamp="2015-11-01T19:00:00Z">
          <nd ref="1"/>
          <nd ref="2"/>
          <tag k="name" v="line"/>
        </way>
        <relation id="1" user="Dummy User" uid="1" version="1" changeset="1" timestamp="2015-11-01T19:00:00Z">
          <member type="node" ref="1" role=""/>
          <member type="way" ref="1" role=""/>
        </relation>
      </osm>
    XML

    (testpath/"test.cpp").write <<~CPP
      #include <cstdlib>
      #include <iostream>
      #include <osmium/io/xml_input.hpp>

      int main(int argc, char* argv[]) {
        osmium::io::File input_file{argv[1]};
        osmium::io::Reader reader{input_file};
        while (osmium::memory::Buffer buffer = reader.read()) {}
        reader.close();
      }
    CPP

    system ENV.cxx, "test.cpp", "-std=c++17", "-lexpat", "-o", "libosmium_read", "-pthread"
    system "./libosmium_read", "test.osm"
  end
end

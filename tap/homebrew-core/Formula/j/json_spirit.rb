class JsonSpirit < Formula
  desc "C++ JSON parser/generator"
  homepage "https://www.codeproject.com/Articles/20027/JSON-Spirit-A-C-JSON-Parser-Generator-Implemented"
  url "https://github.com/png85/json_spirit/archive/refs/tags/json_spirit-4.0.8.tar.gz"
  # Current release is misnamed on GitHub. Previous versioning scheme and
  # homepage dictate the release as "4.08".
  version "4.08"
  sha256 "43829f55755f725c06dd75d626d9e57d0ce68c2f0d5112fe9a01562c0501e94c"
  license "MIT"

  livecheck do
    url :stable
    regex(/^json_spirit[._-]v?(\d+(?:\.\d+)+)$/i)
    strategy :git do |tags, regex|
      # Convert versions like `4.0.8` to `4.08`
      tags.filter_map { |tag| tag[regex, 1]&.gsub(/(\d+)\.(\d+)\.(\d+)/, '\1.\2\3') }
    end
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a3d19c0615d2a6f765324cbe0057796851c994d5eeb0ce0e2575e36f835f8d32"
  end

  depends_on "cmake" => :build
  depends_on "boost" => :no_linkage

  def install
    args = %w[
      -DCMAKE_CXX_STANDARD=14
      -DCMAKE_POLICY_VERSION_MINIMUM=3.5
      -DJSON_SPIRIT_DEMOS=OFF
      -DJSON_SPIRIT_TESTS=OFF
    ]

    system "cmake", "-S", ".", "-B", "build_static", "-DBUILD_STATIC_LIBS=ON", *args, *std_cmake_args
    system "cmake", "--build", "build_static"
    system "cmake", "--install", "build_static"

    system "cmake", "-S", ".", "-B", "build_shared", "-DBUILD_STATIC_LIBS=OFF", *args, *std_cmake_args
    system "cmake", "--build", "build_shared"
    system "cmake", "--install", "build_shared"
  end

  test do
    # https://github.com/png85/json_spirit/blob/master/README.md#writing-json
    (testpath/"test.cpp").write <<~CPP
      #include <json_spirit.h>
      #include <fstream>

      int main(void) {
        json_spirit::Object addr_obj;
        addr_obj.push_back(json_spirit::Pair("house_number", 42));
        addr_obj.push_back(json_spirit::Pair("road", "East Street"));
        addr_obj.push_back(json_spirit::Pair("town", "Newtown"));

        std::ofstream os("address.json");
        write(addr_obj, os, json_spirit::pretty_print);
        os.close();
        return 0;
      }
    CPP

    system ENV.cxx, "-std=c++17", "test.cpp", "-o", "test", "-I#{include}/json_spirit", "-L#{lib}", "-ljson_spirit"
    system "./test"

    expected = { "house_number" => 42, "road" => "East Street", "town" => "Newtown" }
    assert_path_exists testpath/"address.json"
    assert_equal expected, JSON.parse(File.read("address.json"))
  end
end

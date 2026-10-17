class Yyjson < Formula
  desc "High performance JSON library written in ANSI C"
  homepage "https://ibireme.github.io/yyjson/doc/doxygen/html/"
  url "https://github.com/ibireme/yyjson/archive/refs/tags/0.13.0.tar.gz"
  sha256 "34e0f62a2bc11ab20d601e8ca1cc2b2079503aa45119a19133d89d19b94a0fae"
  license "MIT"
  compatibility_version 1
  head "https://github.com/ibireme/yyjson.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/yyjson-0.13.0"
    sha256 cellar: :any, x86_64_windows: "a38f800d7212a546944f9b0770449acadcc868bc6179e1d1f2f003df2520249c"
  end

  depends_on "cmake" => :build

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DBUILD_SHARED_LIBS=ON", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <yyjson.h>

      int main() {
        const char *json = "{\\"name\\":\\"John\\",\\"star\\":4,\\"hits\\":[2,2,1,3]}";
        yyjson_doc *doc = yyjson_read(json, strlen(json), 0);
        yyjson_val *root = yyjson_doc_get_root(doc);

        yyjson_val *name = yyjson_obj_get(root, "name");
        printf("name: %s\\n", yyjson_get_str(name));
        printf("name length: %d\\n", (int)yyjson_get_len(name));

        yyjson_val *star = yyjson_obj_get(root, "star");
        printf("star: %d\\n", (int)yyjson_get_int(star));

        yyjson_val *hits = yyjson_obj_get(root, "hits");
        size_t idx, max;
        yyjson_val *hit;
        yyjson_arr_foreach(hits, idx, max, hit) {
            printf("hit[%d]: %d\\n", (int)idx, (int)yyjson_get_int(hit));
        }

        yyjson_doc_free(doc);
      }
    C

    expected_output = <<~EOS
      name: John
      name length: 4
      star: 4
      hit[0]: 2
      hit[1]: 2
      hit[2]: 1
      hit[3]: 3
    EOS

    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lyyjson", "-o", "test"
    assert_equal expected_output, shell_output(testpath/"test")
  end
end

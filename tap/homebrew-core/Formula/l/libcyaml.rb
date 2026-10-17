class Libcyaml < Formula
  desc "C library for reading and writing YAML"
  homepage "https://github.com/tlsa/libcyaml"
  url "https://github.com/tlsa/libcyaml/archive/refs/tags/v1.4.2.tar.gz"
  sha256 "3211b2a0589ebfe02c563c96adce9246c0787be2af30353becbbd362998d16dc"
  license "ISC"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f74dda61ee0051edbe916c80138e94868e1fb030ff37a769b5ac037f001fc562"
  end

  depends_on "libyaml"

  def install
    system "make", "install", "PREFIX=#{prefix}"
    pkgshare.install "examples/numerical/main.c" => "test.c"
  end

  test do
    flags = %W[
      -I#{include} -I#{formula_opt_include("libyaml")}
      -L#{lib} -L#{formula_opt_lib("libyaml")}
      -lcyaml -lyaml
      -o test
    ]

    system ENV.cc, pkgshare/"test.c", *flags

    (testpath/"test.yaml").write "name: Numbers\ndata:\n- 1\n- 2\n- 4\n- 8\n"
    expected_output = "Numbers:\n  - 1\n  - 2\n  - 4\n  - 8\n"
    assert_equal expected_output, shell_output("#{testpath}/test #{testpath}/test.yaml")
  end
end

class Soplex < Formula
  desc "Optimization package for solving linear programming problems (LPs)"
  homepage "https://soplex.zib.de/", browsed: "2026-09-18"
  url "https://soplex.zib.de/download/release/soplex-8.1.0.tgz"
  sha256 "e7daa1725d9cf01ea1f0a548fa396e7dcc945c1e81af626c2c98503b0c4e5990"
  license "Apache-2.0"

  livecheck do
    url "https://soplex.zib.de/soplexdata.js"
    regex(/["']name["']:\s*?["']soplex[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "54113168da912c6fd56ef06572eb1ac6297f0ee651487fa56f8fa0085eedcf06"
  end

  depends_on "cmake" => :build
  depends_on "boost"
  depends_on "gmp"
  depends_on "mpfr"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def install
    system "cmake", "-S", ".", "-B", "build", "-DPAPILO=OFF", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
    pkgshare.install "src/example.cpp"
  end

  test do
    (testpath/"test.lp").write <<~EOS
      Maximize
       obj: x1 + 2 x2 + 3 x3 + x4
      Subject To
       c1: - x1 + x2 + x3 + 10 x4 <= 20
       c2: x1 - 3 x2 + x3 <= 30
       c3: x2 - 3.5 x4 = 0
      Bounds
       0 <= x1 <= 40
       2 <= x4 <= 3
      General
       x4
      End
    EOS
    assert_match "problem is solved [optimal]",
      shell_output("#{bin}/soplex test.lp")
    assert_match "problem is solved [optimal]",
      shell_output("#{bin}/soplex test.lp -f0 -o0 --readmode=1 --solvemode=2")

    system ENV.cxx, pkgshare/"example.cpp", "-std=c++14", "-L#{lib}", "-I#{include}",
      "-L#{formula_opt_lib("gmp")}", "-L#{formula_opt_lib("mpfr")}",
      "-lsoplex", "-lz", "-lgmp", "-lmpfr", "-o", "test"
    system "./test"
  end
end

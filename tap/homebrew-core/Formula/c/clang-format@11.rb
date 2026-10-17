class ClangFormatAT11 < Formula
  desc "Formatting tools for C, C++, Obj-C, Java, JavaScript, TypeScript"
  homepage "https://clang.llvm.org/docs/ClangFormat.html"
  url "https://github.com/llvm/llvm-project/releases/download/llvmorg-11.1.0/llvm-11.1.0.src.tar.xz"
  sha256 "ce8508e318a01a63d4e8b3090ab2ded3c598a50258cc49e2625b9120d4c03ea5"
  license "Apache-2.0"

  deprecate! date: "2026-02-18", because: :unmaintained, replacement_formula: "clang-format"
  disable! date: "2027-02-18", because: :unmaintained, replacement_formula: "clang-format"

  bottle do
    sha256 cellar: :any, x86_64_windows: "31d81b65721a65dbf932dcf7d3a00a9fbf18c595ff45373337f68dbcd0bead0e"
  end

  depends_on "cmake" => :build

  uses_from_macos "python" => :build
  uses_from_macos "ncurses"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  resource "clang" do
    url "https://github.com/llvm/llvm-project/releases/download/llvmorg-11.1.0/clang-11.1.0.src.tar.xz"
    sha256 "0a8288f065d1f57cb6d96da4d2965cbea32edc572aa972e466e954d17148558b"
  end

  def install
    (buildpath/"tools/clang").install resource("clang")

    system "cmake", "-S", ".", "-B", "build", "-DCMAKE_POLICY_VERSION_MINIMUM=3.5", *std_cmake_args
    system "cmake", "--build", "build", "--target", "clang-format"

    bin.install buildpath/"build/bin/clang-format" => "clang-format-11"
    bin.install buildpath/"tools/clang/tools/clang-format/git-clang-format" => "git-clang-format-11"
  end

  test do
    # NB: below C code is messily formatted on purpose.
    (testpath/"test.c").write <<~C
      int         main(char *args) { \n   \t printf("hello"); }
    C

    assert_equal "int main(char *args) { printf(\"hello\"); }\n",
        shell_output("#{bin}/clang-format-11 -style=Google test.c")
  end
end

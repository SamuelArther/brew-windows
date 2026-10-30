class IcarusVerilog < Formula
  desc "Verilog simulation and synthesis tool"
  homepage "https://steveicarus.github.io/iverilog/"
  url "https://github.com/steveicarus/iverilog/archive/refs/tags/v13_0.tar.gz"
  sha256 "c897bbfa9848688982c6d5c30529fc29d68df0b9ff22ffa73bad89db73a7ce49"
  license all_of: ["GPL-2.0-or-later", "LGPL-2.1-or-later"]
  compatibility_version 1
  head "https://github.com/steveicarus/iverilog.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:[._]\d+)+)$/i)
    strategy :git do |tags, regex|
      tags.filter_map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "052ea1c5fda8247aae4db652083ee3085902013ad03017ad1a6e777d6e991c8e"
  end

  depends_on "autoconf" => :build
  # parser is subtly broken when processed with an old version of bison
  depends_on "bison" => :build

  uses_from_macos "flex" => :build
  uses_from_macos "gperf" => :build
  uses_from_macos "bzip2"

  on_linux do
    depends_on "readline"
    depends_on "zlib-ng-compat"
  end

  # Backport fix for missing mach-o/dyld.h include on macOS 27 SDK
  patch do
    url "https://github.com/steveicarus/iverilog/commit/f20865a5ea4ea7f5cdcbb6d19b0751a9390a8978.patch?full_index=1"
    sha256 "a56837d524f2a7f6bc05435601d7951cd1bdb94fb7ce3c0d43aa774475da6d52"
    type :backport
    resolves "https://github.com/steveicarus/iverilog/pull/1315"
  end

  deny_network_access!

  def install
    system "autoconf"
    system "./configure", "--prefix=#{prefix}"
    system "make", "install", "BISON=#{formula_opt_bin("bison")}/bison"
  end

  test do
    (testpath/"test.v").write <<~VERILOG
      module main;
        initial
          begin
            $display("Boop");
            $finish;
          end
      endmodule
    VERILOG
    system bin/"iverilog", "-o", "test", "test.v"

    expected = <<~EOS
      Boop
      test.v:5: $finish called at 0 (1s)
    EOS
    assert_equal expected, shell_output("./test")

    # test syntax errors do not cause segfaults
    (testpath/"error.v").write "error;"
    expected = <<~EOS
      error.v:1: syntax error
      I give up.
    EOS
    assert_equal expected, shell_output("#{bin}/iverilog error.v 2>&1", 2)
  end
end

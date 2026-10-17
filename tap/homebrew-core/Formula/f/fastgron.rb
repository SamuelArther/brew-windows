class Fastgron < Formula
  desc "High-performance JSON to GRON converter"
  homepage "https://github.com/adamritter/fastgron"
  url "https://github.com/adamritter/fastgron/archive/refs/tags/v0.7.7.tar.gz"
  sha256 "3011a3b99cd07d42648b2e964f459024b13ecc904d30501f0493fb0dc9fc33b2"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/fastgron-0.7.7"
    sha256 cellar: :any, x86_64_windows: "58d100dfa908f639b3b5a78af7676bc2480c0e14e6154edb765e3e0055854813"
  end

  depends_on "cmake" => :build

  uses_from_macos "curl"

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    expected = <<~EOS
      json = []
      json[0] = 3
      json[1] = 4
      json[2] = 5
    EOS
    assert_equal expected, pipe_output(bin/"fastgron", "[3,4,5]")

    assert_match version.to_s, shell_output("#{bin}/fastgron --version 2>&1")
  end
end

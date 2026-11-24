class Toit < Formula
  desc "High-level programming language and SDK"
  homepage "https://toitlang.org/"
  url "https://github.com/toitlang/toit/releases/download/v2.0.0/toit-v2.0.0-with-submodules.tar.gz"
  sha256 "9851b8230031503a737b20c75113546598ae69d89a5ea45b0a084764309a8798"
  license all_of: ["LGPL-2.1-only", "MIT"]

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "dde9d573594cb745aa6cce481871dd123235af59a6c349ae0bdfcc0ecfded401"
  end

  depends_on "cmake" => :build
  depends_on "ninja" => :build

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def install
    # Toit requires its bundled Mbed TLS configuration and allocation-failure
    # patches for garbage collection and retrying TLS operations.
    # Configure directly so Homebrew controls the architecture and compiler flags.
    system "cmake", "-S", ".", "-B", "build", "-G", "Ninja", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    assert_equal "v#{version}", shell_output("#{bin}/toit version").strip

    (testpath/"hello.toit").write <<~TOIT
      main:
        print "Hello, world!"
    TOIT

    assert_equal "Hello, world!\n", shell_output("#{bin}/toit run hello.toit")
    system bin/"toit", "compile", "-o", "hello", "hello.toit"
    assert_equal "Hello, world!\n", shell_output("./hello")
  end
end

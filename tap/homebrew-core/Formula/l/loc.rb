class Loc < Formula
  desc "Count lines of code quickly"
  homepage "https://github.com/cgag/loc"
  url "https://github.com/cgag/loc/archive/refs/tags/v0.4.1.tar.gz"
  sha256 "1e8403fd9a3832007f28fb389593cd6a572f719cd95d85619e7bbcf3dbea18e5"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f93a4a09f025bec079cf1c7461488849a48323b209e9f6f11a0a4871f181c0e3"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"test.cpp").write <<~CPP
      #include <stdio.h>
      int main() {
        println("Hello World");
        return 0;
      }
    CPP
    system bin/"loc", "test.cpp"
  end
end

class Mon < Formula
  desc "Monitor hosts/services/whatever and alert about problems"
  homepage "https://github.com/tj/mon"
  url "https://github.com/tj/mon/archive/refs/tags/1.2.3.tar.gz"
  sha256 "978711a1d37ede3fc5a05c778a2365ee234b196a44b6c0c69078a6c459e686ac"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "875af5fe8d9c1300607c5a6229f9e7f6f948f73122df5b27bdab63c1e3c50c20"
  end

  def install
    bin.mkpath
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    system bin/"mon", "-V"
  end
end

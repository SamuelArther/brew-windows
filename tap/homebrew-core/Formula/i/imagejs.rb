class Imagejs < Formula
  desc "Tool to hide JavaScript inside valid image files"
  homepage "https://github.com/jklmnn/imagejs"
  url "https://github.com/jklmnn/imagejs/archive/refs/tags/0.7.2.tar.gz"
  sha256 "ba75c7ea549c4afbcb2a516565ba0b762b5fc38a03a48e5b94bec78bac7dab07"
  license "GPL-3.0-only"
  head "https://github.com/jklmnn/imagejs.git", branch: "master"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/imagejs-0.7.2"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "5932c3c3a34da84e7b4063bfb8c61fb1061c377e6e36f607bbbc63e189378b7d"
  end

  def install
    system "make"
    bin.install "imagejs"
  end

  test do
    (testpath/"test.js").write "alert('Hello World!')"
    system bin/"imagejs", "bmp", "test.js", "-l"
  end
end

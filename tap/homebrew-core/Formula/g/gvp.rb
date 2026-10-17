class Gvp < Formula
  desc "Go versioning packager"
  homepage "https://github.com/pote/gvp"
  url "https://github.com/pote/gvp/archive/refs/tags/v0.3.0.tar.gz"
  sha256 "e1fccefa76495293350d47d197352a63cae6a014d8d28ebdedb785d4304ee338"
  license "MIT"

  # Upstream fix for "syntax error near unexpected token `;'"
  patch do
    url "https://github.com/pote/gvp/commit/11c4cefd.patch?full_index=1"
    sha256 "19c59c5185d351e05d0b3fbe6a4dba3960c34a804d67fe320e3189271374c494"
    type :backport
    resolves "https://github.com/pote/gvp/pull/46"
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/gvp-0.3.0"
    sha256 cellar: :any, x86_64_windows: "7cc8e96de0c0c589ae4f86f807c7d11db981312d56399fd00e7a751164e95ed6"
  end

  def install
    system "./configure", "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    system bin/"gvp", "in"
    assert File.directory? ".godeps/src"
  end
end

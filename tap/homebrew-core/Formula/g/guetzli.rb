class Guetzli < Formula
  desc "Perceptual JPEG encoder"
  homepage "https://github.com/google/guetzli"
  url "https://github.com/google/guetzli/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "e52eb417a5c0fb5a3b08a858c8d10fa797627ada5373e203c196162d6a313697"
  license "Apache-2.0"
  head "https://github.com/google/guetzli.git", branch: "master"

  deprecate! date: "2026-07-14", because: :repo_archived
  disable! date: "2027-01-14", because: :repo_archived

  bottle do
    sha256 cellar: :any, x86_64_windows: "8d2c0b0eb996608ad6c7e9ea7f64afe199813cf81962ffc210380360c2ac6f9a"
  end

  depends_on "pkgconf" => :build
  depends_on "libpng"

  def install
    system "make"
    bin.install "bin/Release/guetzli"
  end

  test do
    system bin/"guetzli", test_fixtures("test.png"), "test.jpg"
  end
end

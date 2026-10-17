class Fex < Formula
  desc "Powerful field extraction tool"
  homepage "https://www.semicomplete.com/projects/fex/"
  url "https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/semicomplete/fex-2.0.0.tar.gz"
  sha256 "03043c8eac74f43173068a2e693b6f73d5b45f453a063e6da11f34455d0e374e"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "30ca61de980733d8f71f33e9c8ad08cbcac43666b5a26160b5ce8a08b42aad5d"
  end

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    assert_equal "foo", pipe_output("#{bin}/fex 1", "foo bar", 0).chomp
  end
end

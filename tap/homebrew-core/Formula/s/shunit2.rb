class Shunit2 < Formula
  desc "Unit testing framework for Bourne-based shell scripts"
  homepage "https://github.com/kward/shunit2"
  url "https://github.com/kward/shunit2/archive/refs/tags/v2.1.8.tar.gz"
  sha256 "b2fed28ba7282e4878640395284e43f08a029a6c27632df73267c8043c71b60c"
  license "Apache-2.0"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/shunit2-2.1.8"
    sha256 cellar: :any, x86_64_windows: "17b5c57d27854c4ef93a823b94711c7b12237d03e502cd8e42dc16e937f7cd49"
  end

  def install
    bin.install "shunit2"
  end

  test do
    system bin/"shunit2"
  end
end

class Trurl < Formula
  desc "Command-line tool for URL parsing and manipulation"
  homepage "https://curl.se/trurl/"
  url "https://github.com/curl/trurl/releases/download/trurl-0.16.1/trurl-0.16.1.tar.gz"
  sha256 "aac947d4fb421a58abc19a3771e87942cd4721b8f855c433478c94c11a8203ba"
  license "curl"
  head "https://github.com/curl/trurl.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/trurl-0.16.1"
    sha256 cellar: :any, x86_64_windows: "8caa2c8ab3d21e9130c2b7aa39941f122eb3a921e0888b045f4ff06a7c1524aa"
  end

  uses_from_macos "curl", since: :ventura # uses CURLUE_NO_ZONEID, available since curl 7.81.0

  def install
    system "make"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    output = shell_output("#{bin}/trurl https://example.com/hello.html " \
                          "--default-port --get '{scheme} {port} {path}'").chomp
    assert_equal "https 443 /hello.html", output
  end
end

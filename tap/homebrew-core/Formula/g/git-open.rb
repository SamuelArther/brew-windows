class GitOpen < Formula
  desc "Open GitHub webpages from a terminal"
  homepage "https://github.com/jeffreyiacono/git-open"
  url "https://github.com/jeffreyiacono/git-open/archive/refs/tags/v1.3.tar.gz"
  sha256 "a1217e9b0a76382a96afd33ecbacad723528ec1116381c22a17cc7458de23701"
  license "MIT"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/git-open-1.3"
    sha256 cellar: :any, x86_64_windows: "9961940a8b9f8affd47a48ed70698a4a8a09154bc00929726086232c8e05e7e8"
  end

  def install
    bin.install "git-open.sh" => "git-open"
  end

  test do
    system bin/"git-open", "-v"
  end
end

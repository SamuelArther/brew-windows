class Hr < Formula
  desc "<hr />, for your terminal window"
  homepage "https://github.com/LuRsT/hr"
  url "https://github.com/LuRsT/hr/archive/refs/tags/1.5.tar.gz"
  sha256 "d4bb6e8495a8adaf7a70935172695d06943b4b10efcbfe4f8fcf6d5fe97ca251"
  license "MIT"
  head "https://github.com/LuRsT/hr.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/hr-1.5"
    sha256 cellar: :any, x86_64_windows: "db905bf7ba2de427385a2725ea15d3565b079ba4a6d003af1adb777afc03183f"
  end

  def install
    bin.install "hr"
    man1.install "hr.1"
  end

  test do
    system bin/"hr", "-#-"
  end
end

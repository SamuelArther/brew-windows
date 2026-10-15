class Td < Formula
  desc "Your todo list in your terminal"
  homepage "https://github.com/Swatto/td"
  url "https://github.com/Swatto/td/releases/download/1.4.2/td_1.4.2_Windows_x86_64.tar.gz"
  sha256 "3335f242ddbab38d86329ed7a8f3f1e1998a747d217a88ca5caa6ba64ebf9b2f"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Swatto/td/archive/refs/tags/1.4.2.tar.gz"
  end

  def install
    bin.install "td.exe"
  end

  test do
    (testpath/".todos").write "[]\n"
    system bin/"td", "a", "todo of test"
    todos = (testpath/".todos").read
    assert_match "todo of test", todos
    assert_match "pending", todos
  end
end

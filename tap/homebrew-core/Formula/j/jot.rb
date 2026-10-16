class Jot < Formula
  desc "Rapid note management for the terminal"
  homepage "https://github.com/shashwatah/jot"
  url "https://github.com/shashwatah/jot/archive/refs/tags/v0.1.2.tar.gz"
  sha256 "d7da3220c29102ee7c51e2a5656ceb6672ae3b85be22c5ddcd176b330c6029c9"
  license "MIT"
  head "https://github.com/shashwatah/jot.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "161147944f0b55f14a724bf5e58708bd53c0e2345dacafbcc84024059778a0cf"
  end

  depends_on "rust" => :build

  conflicts_with "json-table", because: "both install `jt` binaries"

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    system bin/"jt", "vault", "testvault", testpath
    system bin/"jt", "enter", "testvault"

    system bin/"jt", "note", "testnote"
    assert_path_exists testpath/"testvault/testnote.md"

    system bin/"jt", "remove", "note", "testnote"
    refute_path_exists testpath/"testvault/testnote.md"
  end
end

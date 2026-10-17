class Snooze < Formula
  desc "Run a command at a particular time"
  homepage "https://github.com/leahneukirchen/snooze"
  url "https://github.com/leahneukirchen/snooze/archive/refs/tags/v0.6.tar.gz"
  sha256 "3a4a2f3f00d42e30647d9af79c8e417990ced6c3f0565474b1ca717938b1e2ab"
  license :public_domain

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/snooze-0.6"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b2a8f2114bff739d0c5ac543bbfb9ff152a5448233882ae3d20bd0c4d9863603"
  end

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    assert_match "T00:00:00", shell_output("#{bin}/snooze -n")
  end
end

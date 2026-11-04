class StressNg < Formula
  desc "Stress test a computer system in various selectable ways"
  homepage "https://github.com/ColinIanKing/stress-ng"
  url "https://github.com/ColinIanKing/stress-ng/archive/refs/tags/V0.22.02.tar.gz"
  sha256 "860004ce7484311b3296fc52796fbbc34dfb7f644a769bbd4e03935b7a1b0ccb"
  license "GPL-2.0-or-later"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "76d9d7a9f021197921241914aafa68311fe5a5aeac766b2cfeb8199b5fd521ec"
  end

  uses_from_macos "libxcrypt"

  on_linux do
    depends_on "acl"
    depends_on "gmp"
    depends_on "zlib-ng-compat"
  end

  def install
    # disable target_clones so no non-baseline (AVX-512) code lands in the bottle
    ENV.append_to_cflags "-DHAVE_BUILD_SMALL" if Hardware::CPU.intel?

    inreplace "Makefile" do |s|
      s.gsub! "/usr", prefix
      s.change_make_var! "BASHDIR", prefix/"etc/bash_completion.d"
    end
    system "make"
    system "make", "install"
    bash_completion.install "bash-completion/stress-ng"
  end

  test do
    output = shell_output("#{bin}/stress-ng -c 1 -t 1 2>&1")
    assert_match "successful run completed", output
  end
end

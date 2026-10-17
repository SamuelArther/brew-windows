class Posh < Formula
  desc "Policy-compliant ordinary shell"
  homepage "https://salsa.debian.org/clint/posh"
  url "https://salsa.debian.org/clint/posh/-/archive/debian/0.14.6/posh-debian-0.14.6.tar.bz2"
  sha256 "cacc5eeebcc36d83a2b5d63fd4d58b17762a3e294aa87d5d8af5c3f0dc21272f"
  license "GPL-3.0-or-later"

  livecheck do
    url :stable
    regex(%r{^debian/v?(\d+(?:\.\d+)+)$}i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "d7b5fa9a580aa25c89dacfb493eaf1facdbd250751ce4bc18ac30faa88072451"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build

  def install
    # Upstream still uses K&R function definitions, which do not compile as C23.
    ENV["ac_cv_prog_cc_c23"] = "no"
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    output = shell_output("#{bin}/posh -c 'echo homebrew'")
    assert_equal "homebrew", output.chomp
  end
end

class Sassc < Formula
  desc "Wrapper around libsass that helps to create command-line apps"
  homepage "https://github.com/sass/sassc"
  url "https://github.com/sass/sassc.git",
      tag:      "3.6.2",
      revision: "66f0ef37e7f0ad3a65d2f481eff09d09408f42d0"
  license "MIT"
  head "https://github.com/sass/sassc.git", branch: "master"

  deprecate! date: "2026-04-02", because: :repo_archived
  disable! date: "2027-04-02", because: :repo_archived

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0a7ceeb0bd9ae1dd83c92911bfa7959cd23738b2828072ba8b5df30e5a710f71"
  end

  depends_on "autoconf" => :build
  depends_on "automake" => :build
  depends_on "libtool" => :build
  depends_on "pkgconf" => :build
  depends_on "libsass"

  def install
    system "autoreconf", "--force", "--install", "--verbose"
    system "./configure", "--disable-silent-rules", *std_configure_args
    system "make", "install"
  end

  test do
    (testpath/"input.scss").write <<~SCSS
      div {
        img {
          border: 0px;
        }
      }
    SCSS

    assert_equal "div img{border:0px}",
    shell_output("#{bin}/sassc --style compressed input.scss").strip
  end
end

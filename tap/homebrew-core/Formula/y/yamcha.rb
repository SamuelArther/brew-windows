class Yamcha < Formula
  desc "NLP text chunker using Support Vector Machines"
  homepage "http://chasen.org/~taku/software/yamcha/"
  # Upstream is only available via HTTP, so we prefer pkgsrc's HTTPS mirror
  url "https://cdn.netbsd.org/pub/pkgsrc/distfiles/yamcha-0.33.tar.gz"
  mirror "http://chasen.org/~taku/software/yamcha/src/yamcha-0.33.tar.gz"
  sha256 "413d4fc0a4c13895f5eb1468e15c9d2828151882f27aea4daf2399c876be27d5"
  license "LGPL-2.1-or-later"

  # Last release on 2005-09-05
  deprecate! date: "2026-07-11", because: :unmaintained
  disable! date: "2026-10-11", because: :unmaintained

  depends_on "tinysvm"

  on_arm do
    # Added automake as a build dependency to update config files for ARM support.
    depends_on "automake" => :build
  end

  # Use Debian patch to fix error: no matching function for call to 'make_pair'
  patch do
    url "https://snapshot.debian.org/archive/debian-debug/20170926T092903Z/pool/main/y/yamcha/yamcha_0.33-2.debian.tar.xz"
    sha256 "6a114584af8256504a195e4164308bdd462d1e8860b77951431b4ca7de3a21e8"
    type :unofficial
    apply "patches/1011_fix_gcc7_compilation.patch"
  end

  def install
    if Hardware::CPU.arm?
      # Workaround for ancient config files not recognizing aarch64 macos.
      %w[config.guess config.sub].each do |fn|
        cp Formula["automake"].share/"automake-#{Formula["automake"].version.major_minor}"/fn, fn
      end
    end

    # Work around failure because of missing #include <cstring>/<cstdlib> on Linux.
    ENV.append "CXX", "-include cstring -include cstdlib" if OS.linux?

    system "./configure", "--mandir=#{man}", *std_configure_args
    system "make", "install"
  end

  test do
    libexecdir = shell_output("#{bin}/yamcha-config --libexecdir").chomp
    assert_equal libexecdir, "#{libexec}/yamcha"

    (testpath/"train.data").write <<~EOS
      He        PRP  B-NP
      reckons   VBZ  B-VP
      the       DT   B-NP
      current   JJ   I-NP
      account   NN   I-NP
      deficit   NN   I-NP
      will      MD   B-VP
      narrow    VB   I-VP
      to        TO   B-PP
      only      RB   B-NP
      #         #    I-NP
      1.8       CD   I-NP
      billion   CD   I-NP
      in        IN   B-PP
      September NNP  B-NP
      .         .    O

      He        PRP  B-NP
      reckons   VBZ  B-VP
      ..
    EOS

    system "make", "-j", "1",
                   "-f", "#{libexecdir}/Makefile",
                   "CORPUS=train.data", "MODEL=case_study", "train"

    %w[log model se svmdata txtmodel.gz].each do |ext|
      assert_path_exists testpath/"case_study.#{ext}"
    end
  end
end

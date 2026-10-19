class Textidote < Formula
  desc "Spelling, grammar and style checking on LaTeX documents"
  homepage "https://sylvainhalle.github.io/textidote"
  url "https://github.com/sylvainhalle/textidote/archive/refs/tags/v0.9.tar.gz"
  sha256 "df4ec98e355dddb3cebe155868da63ae17f48778151d2d8b227d412aee768c9b"
  license "GPL-3.0-or-later"
  head "https://github.com/sylvainhalle/textidote.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "fe841cc265426bd8012009364a27288df8f8cb4ee3d276802d1ad14af5c44b0f"
  end

  depends_on "ant" => :build
  depends_on "openjdk"

  deny_network_access!

  def fetch
    # Skip javadoc: JDK 25 doclint rejects textidote's legacy `<tt>` tags
    inreplace "build.xml", 'depends="compile,javadoc"', 'depends="compile"'

    system "ant", "download-deps"
  end

  def install
    # Avoid downloading junit as we don't run tests
    system "ant", "-Dbuild.targetjdk=#{Formula["openjdk"].version.major}", "-Djunit.absent=false"

    # Install the JAR + a wrapper script
    libexec.install "textidote-#{version}.jar" => "textidote.jar"
    # Fix run with `openjdk` 24.
    # Reported upstream at https://github.com/sylvainhalle/textidote/issues/265.
    bin.write_jar_script libexec/"textidote.jar", "textidote", "-Djdk.xml.totalEntitySizeLimit=50000000"

    bash_completion.install "Completions/textidote.bash" => "textidote"
    zsh_completion.install "Completions/textidote.zsh" => "_textidote"
  end

  test do
    output = shell_output("#{bin}/textidote --version")
    assert_match "TeXtidote", output

    (testpath/"test1.tex").write <<~'TEX'
      \documentclass{article}
      \begin{document}
        This should fails.
      \end{document}
    TEX

    output = shell_output("#{bin}/textidote --check en #{testpath}/test1.tex", 1)
    assert_match "The modal verb 'should' requires the verb's base form..", output

    (testpath/"test2.tex").write <<~'TEX'
      \documentclass{article}
      \begin{document}
        This should work.
      \end{document}
    TEX

    output = shell_output("#{bin}/textidote --check en #{testpath}/test2.tex")
    assert_match "Everything is OK!", output
  end
end

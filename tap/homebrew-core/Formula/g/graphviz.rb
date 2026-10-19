class Graphviz < Formula
  desc "Graph visualization software from AT&T and Bell Labs"
  homepage "https://graphviz.org/"
  url "https://gitlab.com/api/v4/projects/4207231/packages/generic/graphviz-releases/16.1.0/windows_10_cmake_Release_Graphviz-16.1.0-win64.zip"
  sha256 "733e49626c492242eb8dca30ea627b6ead20710e207998c7933b4909d92d6abc"
  license "EPL-1.0"
  revision 1
  version_scheme 1

  def install
    install_tree
    %w[acyclic bcomps ccomps circo dijkstra dot fdp gc gvcolor gvgen gvpack gvpr neato nop osage
       patchwork sccmap sfdp tred twopi unflatten].each do |cmd|
      windows_exe "libexec/bin/#{cmd}.exe", cmd
    end

    # The libraries for dependents (vala's libgvc): headers, import libraries and .pc files in the keg itself,
    # where pkgconf's prefix relocation (pcfiledir/../..) points; the release's .pc files name
    # C:/Program Files/Graphviz. Its DLLs sit beside bin's commands, where dependents' builds find them.
    include.mkpath
    lib.mkpath
    cp_r (libexec/"include").children, include
    cp_r (libexec/"lib").children, lib
    (lib/"pkgconfig").glob("*.pc").each { |pc| inreplace pc, "C:/Program Files/Graphviz", "${pcfiledir}/../.." }
    (libexec/"bin").glob("*.dll").each { |dll| bin.install_symlink dll }
  end

  test do
    (testpath/"sample.dot").write <<~EOS
      digraph G {
        a -> b
      }
    EOS

    system bin/"dot", "-Tpdf", "-o", "sample.pdf", "sample.dot"
    assert_match "Version: #{version}", (lib/"pkgconfig/libgvc.pc").read
    refute_match "Program Files", (lib/"pkgconfig/libgvc.pc").read
  end
end

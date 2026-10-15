class Ruby < Formula
  desc "Powerful, clean, object-oriented scripting language"
  homepage "https://www.ruby-lang.org/"
  url "https://github.com/oneclick/rubyinstaller2/releases/download/RubyInstaller-4.0.7-1/rubyinstaller-4.0.7-1-x64.7z"
  version "4.0.7"
  sha256 "7a9be96d3448ecbd9a81eccae1f619131dfcc761f2c98dded03ece6c05d0474f"
  license "Ruby"

  livecheck do
    url :stable
    regex(/^RubyInstaller[._-]v?(\d+(?:\.\d+)+)(?:-\d+)?$/i)
    strategy :github_latest
  end

  # ruby-lang.org publishes source only and recommends RubyInstaller for Windows. This is its
  # plain 7z archive (no installer, no MSYS2 devkit; gems with C extensions need `ridk install`).

  def install
    # brew does not unpack .7z itself; Windows' bundled bsdtar reads 7z archives.
    archive = buildpath/"rubyinstaller-#{version}-1-x64.7z"
    system "#{ENV["SystemRoot"]}/System32/tar.exe", "-xf", archive, "-C", buildpath
    install_tree "rubyinstaller-#{version}-1-x64", "libexec"
    windows_exe "libexec/bin/ruby.exe", "ruby"
    windows_exe "libexec/bin/rubyw.exe", "rubyw"
    windows_cmd "libexec/bin/gem.cmd", "gem"
    %w[bundle bundler erb irb racc rake rbs rdbg rdoc ri].each do |cmd|
      windows_cmd "libexec/bin/#{cmd}.bat", cmd
    end
  end

  test do
    output = shell_output("#{bin}/ruby -e \"puts 'hello'\"")
    assert_equal "hello\n", output
    assert_match version.to_s, shell_output("#{bin}/ruby --version")
    system bin/"gem", "--version"
  end
end

﻿using System;
using BuildNotifications.PluginInterfaces.Configuration.Options;
using Xunit;

namespace BuildNotifications.Tests.PluginInterfaces.Configuration.Options;

public class ValueOptionTests
{
    private class TestValueOption : ValueOption<object>
    {
        public TestValueOption()
            : base(new object(), string.Empty, string.Empty)
        {
        }

        public bool Validated { get; private set; }

        public void SetValue(object value)
        {
            Value = value;
        }

        protected override bool ValidateValue(object value)
        {
            Validated = true;
            return base.ValidateValue(value);
        }
    }

    private class TestValueOption<T> : ValueOption<T>
    {
        public TestValueOption(T value = default)
            : base(value, string.Empty, string.Empty)
        {
        }
    }

    [Fact]
    public void SettingValueShouldCallValidateFunc()
    {
        // Arrange
        var sut = new TestValueOption();

        // Act
        sut.Value = new object();

        // Assert
        Assert.True(sut.Validated);
    }

    [Fact]
    public void SettingValueShouldNotRaiseEventWhenValueIsSame()
    {
        // Arrange
        var sut = new TestValueOption();
        var received = false;
        sut.ValueChanged += (_, _) => received = true;

        // Act
        sut.SetValue(sut.Value);

        // Assert
        Assert.False(received);
    }

    [Fact]
    public void SettingValueShouldRaiseEvent()
    {
        // Arrange
        var sut = new TestValueOption();
        var newValue = new object();

        // Act
        var evt = Assert.RaisesAny<EventArgs>(
            e => sut.ValueChanged += e,
            e => sut.ValueChanged -= e,
            () => sut.Value = newValue);

        // Assert
        Assert.NotNull(evt);
        Assert.Same(sut, evt.Sender);
    }

    [Fact]
    public void SettingValueShouldSetValueWhenCalledByInterface()
    {
        // Arrange
        var sut = new TestValueOption<string>();
        var iSut = (IValueOption)sut;

        const string expected = "test";

        // Act
        iSut.Value = expected;

        // Assert
        Assert.Equal(expected, iSut.Value);
    }

    [Fact]
    public void SettingValueShouldThrowWhenGenericTypeDoesNotMatch()
    {
        // Arrange
        var sut = new TestValueOption<string>();
        var iSut = (IValueOption)sut;

        // Act
        var ex = Record.Exception(() => iSut.Value = 123);

        // Assert
        Assert.IsType<ArgumentException>(ex);
    }
}